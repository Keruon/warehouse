import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Alert, Button, Card, Form, Input, Typography } from 'antd';
import useAuth from '../hooks/useAuth';

const { Title } = Typography;

type LoginFormValues = {
  usernameOrEmail: string;
  password: string;
};

export default function LoginPage(): React.ReactElement {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [error, setError] = useState<string>('');
  const [submitting, setSubmitting] = useState<boolean>(false);

  async function handleSubmit(values: LoginFormValues): Promise<void> {
    setSubmitting(true);
    setError('');

    try {
      await login(values.usernameOrEmail, values.password);
      navigate('/dashboard', { replace: true });
    } catch (submitError: unknown) {
      const message = submitError instanceof Error ? submitError.message : 'Login failed. Check credentials and try again.';
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="login-page">
      <Card className="login-card" bordered>
        <Title level={3}>Warehouse Stock Login</Title>
        {error ? <Alert type="error" message={error} style={{ marginBottom: 16 }} /> : null}
        <Form<LoginFormValues> layout="vertical" onFinish={handleSubmit}>
          <Form.Item
            label="Username or Email"
            name="usernameOrEmail"
            rules={[{ required: true, message: 'Please enter username or email.' }]}
          >
            <Input autoComplete="username" />
          </Form.Item>

          <Form.Item
            label="Password"
            name="password"
            rules={[{ required: true, message: 'Please enter password.' }]}
          >
            <Input.Password autoComplete="current-password" />
          </Form.Item>

          <Button htmlType="submit" type="primary" loading={submitting} block>
            Sign in
          </Button>
        </Form>
      </Card>
    </div>
  );
}